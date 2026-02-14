import React from 'react'
import { Card } from '../ui/Card'
import { Avatar } from '../ui/Avatar'
import { Icon } from '../ui/Icon'
import { Button } from '../ui/Button'
import { BOOKING_STATUS } from '../../utils/constants'

export const BookingCard = ({ 
  doctorName, 
  specialty, 
  clinic, 
  location, 
  date, 
  time, 
  status,
  avatar,
  onReschedule,
  onCancel
}) => {
  const getStatusColor = (status) => {
    switch (status) {
      case BOOKING_STATUS.UPCOMING:
        return 'text-[#1E2A38] bg-[#1E2A38]/10'
      case BOOKING_STATUS.COMPLETED:
        return 'text-green-700 bg-green-100'
      case BOOKING_STATUS.CANCELED:
        return 'text-red-700 bg-red-100'
      default:
        return 'text-gray-600 bg-gray-100'
    }
  }

  const getStatusText = (status) => {
    switch (status) {
      case BOOKING_STATUS.UPCOMING:
        return 'Upcoming'
      case BOOKING_STATUS.COMPLETED:
        return 'Completed'
      case BOOKING_STATUS.CANCELED:
        return 'Canceled'
      default:
        return status
    }
  }

  return (
    <Card elevation="medium" className="p-5">
      <div className="flex items-start gap-4">
        <Avatar src={avatar} alt={doctorName} size={56} />
        
        <div className="flex-1 min-w-0">
          <div className="flex justify-between items-start">
            <div className="min-w-0">
              <h3 className="text-[16px] font-semibold text-gray-900 mb-1">
                {doctorName}
              </h3>
              <p className="text-[14px] text-gray-600 mb-2">{specialty}</p>
              <p className="text-[14px] text-gray-500 mb-1 flex items-center gap-1">
                <Icon name="mapPin" size={14} />
                {clinic}
              </p>
              <p className="text-[14px] text-gray-500 flex items-center gap-1">
                <Icon name="mapPin" size={14} />
                {location}
              </p>
            </div>
            
            <div className="text-right flex-shrink-0">
              <div className="inline-block px-3 py-1 rounded-full text-xs font-medium mb-2" style={{ backgroundColor: status === BOOKING_STATUS.UPCOMING ? '#1E2A38' : status === BOOKING_STATUS.COMPLETED ? '#DCFCE7' : '#FEE2E2', color: status === BOOKING_STATUS.UPCOMING ? '#FFFFFF' : status === BOOKING_STATUS.COMPLETED ? '#166534' : '#991B1B' }}>
                {getStatusText(status)}
              </div>
              <p className="text-[14px] text-gray-700 font-medium">{date}</p>
              <p className="text-[14px] text-gray-600">{time}</p>
            </div>
          </div>
          
          {status === BOOKING_STATUS.UPCOMING && (
            <div className="mt-4 pt-4 border-t border-[#EAEAEA]">
              <ButtonGroup>
                <Button variant="primary" size="medium" onClick={onReschedule}>
                  Reschedule
                </Button>
                <Button variant="secondary" size="medium" onClick={onCancel}>
                  Cancel
                </Button>
              </ButtonGroup>
            </div>
          )}
        </div>
      </div>
    </Card>
  )
}

export const BookingCardSkeleton = () => {
  return (
    <Card elevation="medium" className="p-5">
      <div className="flex items-start gap-4">
        <div 
          className="rounded-lg flex-shrink-0"
          style={{ width: '56px', height: '56px', backgroundColor: '#EAEAEA' }}
        />
        
        <div className="flex-1 min-w-0">
          <div className="flex justify-between items-start">
            <div className="min-w-0">
              <div 
                className="rounded-lg mb-2"
                style={{ width: '150px', height: '20px', backgroundColor: '#EAEAEA' }}
              />
              <div 
                className="rounded-lg mb-2"
                style={{ width: '120px', height: '14px', backgroundColor: '#EAEAEA' }}
              />
              <div 
                className="rounded-lg mb-1"
                style={{ width: '180px', height: '14px', backgroundColor: '#EAEAEA' }}
              />
              <div 
                className="rounded-lg"
                style={{ width: '200px', height: '14px', backgroundColor: '#EAEAEA' }}
              />
            </div>
            
            <div className="text-right flex-shrink-0">
              <div 
                className="rounded-full mb-2"
                style={{ width: '60px', height: '20px', backgroundColor: '#EAEAEA' }}
              />
              <div 
                className="rounded-lg mb-1"
                style={{ width: '80px', height: '14px', backgroundColor: '#EAEAEA' }}
              />
              <div 
                className="rounded-lg"
                style={{ width: '70px', height: '14px', backgroundColor: '#EAEAEA' }}
              />
            </div>
          </div>
        </div>
      </div>
    </Card>
  )
}

const ButtonGroup = ({ children }) => (
  <div className="flex gap-3">{children}</div>
)
